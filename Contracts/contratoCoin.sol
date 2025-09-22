// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract SalesContract {
    address public buyer;
    address public seller;
    address public bank;
    address public carrier;
    uint public paymentAmount;
    uint public shippingCosts;

    // condições para garantir concorrência das ações
    bool private _isSent;
    bool private _isShippingCostsPaid;
    bool private _isProductRecieptNotified;
    bool private _isProductDeliveryNotified;

    enum ContractState {
        Created,
        ProductBought,
        ProductPaid,
        PaymentNotified,
        ProductShipped,
        ShippingPaid,
        ProductDelivered,
        ReceiptConfirmed,
        DeliveryConfirmed,
        PaymentReleasedCarrier,
        PaymentConfirmedCarrier
    }

    ContractState public state;

    event notify(address sender, address reciever, string message);

    constructor(
        address _buyer,
        address _seller,
        address _bank,
        address _carrier,
        uint _paymentAmount,
        uint _shippingCosts
    ) {
        buyer = _buyer;
        seller = _seller;
        bank = _bank;
        carrier = _carrier;
        paymentAmount = _paymentAmount;
        state = ContractState.Created;
        shippingCosts = _shippingCosts;
    }

    modifier atState(ContractState _requiredState) {
        require(state == _requiredState, "Invalid state for this action");
        _;
    }

    modifier onlyB() {
        require(msg.sender == buyer);
        _;
    }

    modifier onlyS() {
        require(msg.sender == seller);
        _;
    }

    modifier onlyC() {
        require(msg.sender == carrier);
        _;
    }

    modifier onlyK() {
        require(msg.sender == bank);
        _;
    }

    modifier FirstInternalRulesBank() {
        require(
            state == ContractState.ReceiptConfirmed,
            "Invalid state for this action, first internal bank rule"
        );
        _;
    }

    modifier SecondInternalRulesBank() {
        require(
            state == ContractState.ReceiptConfirmed,
            "Invalid state for this action, second internal bank rule"
        );
        _;
    }

    modifier FirstInternalRulesCarrier() {
        require(
            state == ContractState.ShippingPaid,
            "Invalid state for this action, first internal carrier rule"
        );
        _;
    }

    function buyProduct() external atState(ContractState.Created) onlyB {
        state = ContractState.ProductBought;

        emit notify(buyer, seller, "Buyer bought product from seller");
    }

    function payProduct(
        uint256 _payment
    ) external atState(ContractState.ProductBought) onlyB {
        require(_payment == paymentAmount, "Incorrect payment amount");
        state = ContractState.ProductPaid;
        emit notify(buyer, bank, "Buyer sent payment to bank");
    }

    function notifyProductPayment()
        external
        atState(ContractState.ProductPaid)
        onlyK
    {
        state = ContractState.PaymentNotified;
        emit notify(bank, seller, "Bank notified the payment to the seller");
    }

    function sendProduct()
        external
        FirstInternalRulesCarrier
        SecondInternalRulesBank
    {
        state = ContractState.ProductShipped;
        emit notify(
            carrier,
            buyer,
            "Carrier is in route deliver product to buyer"
        );
        state = ContractState.ProductDelivered;
        emit notify(carrier, buyer, "Carrier delivered product to buyer");
    }

    function payShippingCosts(
        uint256 _shippingPayment
    ) external atState(ContractState.PaymentNotified) onlyS {
        require(_shippingPayment == shippingCosts);
        state = ContractState.ShippingPaid;
        emit notify(seller, bank, "Seller paid for shipping costs");
    }

    function deliverProduct()
        external
        atState(ContractState.ProductShipped)
        onlyC
    {
        state = ContractState.ProductDelivered;
        emit notify(seller, bank, "Carrier delivered product to buyer");
    }

    function notifyProductReceipt()
        external
        atState(ContractState.ProductDelivered)
        onlyB
    {
        state = ContractState.ReceiptConfirmed;
        emit notify(buyer, bank, "Buyer confirmed product delivery");
    }

    function notifyProductDelivery()
        external
        atState(ContractState.ProductDelivered)
        onlyC
    {
        state = ContractState.DeliveryConfirmed;
        emit notify(carrier, seller, "Carrier confirmed product delivery");
    }

    function payProductSeller()
        external
        atState(ContractState.ReceiptConfirmed)
        onlyK
    {
        emit notify(bank, seller, "Bank sent product payment to seller");
    }

    function liberateShippingCosts()
        external
        atState(ContractState.ProductDelivered)
        onlyS
    {
        state = ContractState.PaymentReleasedCarrier;
        emit notify(
            seller,
            bank,
            "Seller signalized to liberate shipping costs to carrier"
        );
    }

    function payShippingCosts()
        external
        atState(ContractState.PaymentReleasedCarrier)
        onlyK
    {
        state = ContractState.PaymentConfirmedCarrier;
        emit notify(bank, carrier, "Bank sent carrier the shipping costs");
    }
}
