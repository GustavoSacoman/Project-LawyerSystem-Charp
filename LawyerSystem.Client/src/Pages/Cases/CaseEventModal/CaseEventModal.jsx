import React from 'react';
import { Modal, Button, Form, Input, Select, List } from 'antd';
import { ToastContainer, toast } from 'react-toastify';
import 'antd/dist/antd.css';
import './CaseEventModal.css';

const { TextArea } = Input;
const { Option } = Select;

const CaseEventModal = ({ isOpen, onClose, caseId }) => {
    const [events, setEvents] = React.useState([]);
    const [antdForm] = Form.useForm();

    const showError = (message) => {
        toast.error(message, {
            position: 'top-right',
            autoClose: 3000,
            hideProgressBar: false,
            closeOnClick: true,
            pauseOnHover: false,
            draggable: true,
        });
    };

    const handleSubmit = async (values) => {
        try {

            if (!caseId) {
                showError('Erro: ID do caso n�o foi definido.');
                return;
            }
            const formattedDate = new Date(values.EventDate).toISOString();
            const data = {
                Title: values.Title,
                Description: values.Description,
                EventDate: formattedDate,
                EventType: parseInt(values.EventType, 10),
                EventStatus: parseInt(values.EventStatus, 10),
                Notes: values.Notes,
                CaseId: caseId, 
            };
            
            // Convert EventType and EventStatus to integers
            data.EventType = parseInt(data.EventType, 10);
            data.EventStatus = parseInt(data.EventStatus, 10);

            console.log('Submitting event data:', data);

            const response = await fetch(`http://localhost:5000/api/caseEvent/create`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data),
            });

            if (!response.ok) {
                showError('Erro ao enviar o formul�rio. Por favor, tente novamente.');
                return;
            }

            // Reset the form and optionally update the events list
            antdForm.resetFields();
        } catch (ex) {
            console.error('Error submitting form:', ex);
            showError('Erro ao enviar o formul�rio. Por favor, tente novamente.');
        }
    };

    return (
        <Modal
            open={isOpen}
            title="Eventos do Caso"
            onCancel={onClose}
            footer={null}
            className="case-event-modal"
        >
            <div className="events-list">
                <List
                    dataSource={events}
                    renderItem={(item) => (
                        <List.Item key={item.id}>
                            <List.Item.Meta
                                title={`${item.Title} em ${item.EventDate}`}
                                description={item.Description}
                            />
                            <div>{item.Notes}</div>
                        </List.Item>
                    )}
                />
            </div>
            <Form
                form={antdForm}
                layout="vertical"
                onFinish={handleSubmit}
            >
                <Form.Item
                    name="Title"
                    label="T�tulo"
                    rules={[{ required: true, message: 'Por favor, insira um t�tulo' }]}
                >
                    <Input />
                </Form.Item>
                <Form.Item
                    name="Description"
                    label="Descri��o"
                    rules={[{ required: true, message: 'Por favor, insira uma descri��o' }]}
                >
                    <Input />
                </Form.Item>
                <Form.Item
                    name="EventDate"
                    label="Data do Evento"
                    rules={[{ required: true, message: 'Por favor, selecione uma data' }]}
                >
                    <Input type="datetime-local" />
                </Form.Item>
                <Form.Item
                    name="EventType"
                    label="Tipo de Evento"
                    rules={[{ required: true, message: 'Por favor, selecione um tipo de evento' }]}
                >
                    <Select placeholder="Selecione o tipo de evento">
                        <Option value="0">Reuni�o</Option>
                        <Option value="1">Audi�ncia</Option>
                        <Option value="2">Peti��o</Option>
                        <Option value="3">Senten�a</Option>
                        <Option value="4">Despacho</Option>
                    </Select>
                </Form.Item>
                <Form.Item
                    name="EventStatus"
                    label="Status do Evento"
                    rules={[{ required: true, message: 'Por favor, selecione um status' }]}
                >
                    <Select placeholder="Selecione o status do evento">
                        <Option value="0">Agendado</Option>
                        <Option value="1">Realizado</Option>
                        <Option value="2">Cancelado</Option>
                        <Option value="3">Adiado</Option>
                        <Option value="4">Outro</Option>
                    </Select>
                </Form.Item>
                <Form.Item name="Notes" label="Notas">
                    <TextArea rows={3} />
                </Form.Item>
                <Form.Item>
                    <Button type="primary" htmlType="submit">
                        Adicionar Evento
                    </Button>
                </Form.Item>
            </Form>
            <ToastContainer />
        </Modal>
    );
};

export default CaseEventModal;
